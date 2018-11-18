import * as expect from "expect";
import NeoJs from "../NeoJs";
import Aigle from "aigle";
import * as _ from 'lodash';

let neo = new NeoJs({
    scriptHash: 'f837538ebaa0d5272e304edbf085175441beb82f' //token expiration #18.11.18
});

let addressAsByteArray = neo.sc.ContractParam.byteArray(neo.config.myAddress, 'address');
let otherAddress = neo.sc.ContractParam.byteArray('Aea1mQwHmpBGU6Ss6Y2qX3hAX6jooKiXBX', 'address');
let hasMinted = false;
describe("Token Expiration", function () {
    this.timeout(50000);
    before(async () => {
        // await neo.call('mintToken', [neo.sc.ContractParam.byteArray(neo.config.myAddress, 'address')]);

        let result = await neo.get('totalSupply', []);
        if (result[0].value === '') {
            console.log('MINTING 4 TOKENS!!!');
            await neo.call('mintToken', [addressAsByteArray]);
            await neo.call('mintToken', [addressAsByteArray]);
            await neo.call('mintToken', [addressAsByteArray]);
            await neo.call('mintToken', [addressAsByteArray]);
            hasMinted = true;
        }
    });
    it('should return 04 balanceOf for my address', async () => {
        let result = await neo.get('balanceOf', [addressAsByteArray]);
        expect(result[0].value).toEqual('04');
    });
    it('should return empty balanceOf for wrong address', async () => {
        let result = await neo.get('balanceOf', [neo.sc.ContractParam.byteArray(neo.config.myAddress.replace('A','S'), 'address')]);
        expect(result[0].value).toEqual('');
    });
    it('should return 04 totalSupply', async () => {
        let result = await neo.get('totalSupply', [addressAsByteArray]);
        expect(result[0].value).toEqual('04');
    });


    describe("tokens of owner", function () {
        let tokenIds;
        before(async () => {
            let tokensOfOwnerResult = await neo.get('tokensOfOwner', [addressAsByteArray]);
            tokenIds = _.map(tokensOfOwnerResult[0].value, 'value');
        });
        it('4 tokens minted', async () => {
            expect(tokenIds.length).toEqual(4);
        });
        it('owner of tokens is me', async () => {
            await Aigle.forEach(tokenIds, async (tokenId) => {
                let tokensOfOwnerResult = await neo.get('ownerOf', [tokenId]);
                expect(tokensOfOwnerResult[0].value).toEqual(addressAsByteArray.value);
            });
        });

        describe('Lend', () => {
            before(async () => {
                if(hasMinted) {
                    return;
                }
                // console.log('Lending TOKEN #1');
                // await neo.call('lend', [addressAsByteArray, otherAddress, tokenIds[1]]);
            });

            it('should not have active Lend on fresh token', async () => {
                let result = await neo.get('isLendActive', [tokenIds[0]]);
                expect(result[0].value).toEqual('');
            });

            it('should have Lend active on Lent token', async () => {
                let result = await neo.get('isLendActive', [tokenIds[1]]);
                expect(result[0].value).toEqual('1');
            });
        });

        describe('Lend short period and return', () => {
            before(async () => {
                if(hasMinted) {
                    return;
                }
                // console.log('Lending TOKEN #3');
                // await neo.call('lend', [addressAsByteArray, otherAddress, tokenIds[3], -1]);
                // console.log('ReturnToOwner TOKEN #3');
                // await neo.call('returnToOwner', [tokenIds[3]]);
            });

            it('should not have active Lend on fresh token', async () => {
                let result = await neo.get('isLendActive', [tokenIds[0]]);
                expect(result[0].value).toEqual('');
            });

            it('should still have lend on long token', async () => {
                let result = await neo.get('isLendActive', [tokenIds[1]]);
                expect(result[0].value).toEqual('1');
            });

            it('should not have active Lend on returned token', async () => {
                let result = await neo.get('isLendActive', [tokenIds[3]]);
                expect(result[0].value).toEqual('');
            });

            it('returned token should have the ownerOf the original owner', async () => {
                let result = await neo.get('ownerOf', [tokenIds[3]]);
                expect(result[0].value).toEqual(addressAsByteArray.value);
            });
        });
    });
});
